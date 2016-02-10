// DDsavetool.cpp : Defines the exported functions for the DLL application.
//

#include "DDsavetool.h"

#define WIN32_LEAN_AND_MEAN
#include <windows.h>
#include <stdio.h>
#include <tchar.h>
#include <stdlib.h>

#include "easyzlib.h"

/*
Notes:
- Conversion only works one way: console to pc. And conversion only works with Dark Arisen savegames.
*/

//Size of full file is always 524288 (extra data are nulls)
#define SAVESIZE 524288
#define MAXPATH 260

__declspec(dllexport) int ERR_READ = 1;
__declspec(dllexport) int ERR_WRITE = 2;
__declspec(dllexport) int ERR_FORMAT = 3;
__declspec(dllexport) int ERR_UNPACK = 4;

#pragma pack(push, 1)
struct header_s
{
	unsigned int u1; //Version (21 for DDDA console and DDDA PC, and 5 for original DD on console)
	unsigned int realSize; //Real size of compressed save game data
	unsigned int compressedSize;
	unsigned int u2; //Always 860693325
	unsigned int u3; //Always 0
	unsigned int u4; //Always 860700740
	unsigned int hash; //Checksum of compressed save data
	unsigned int u5; //Always 1079398965
};
#pragma pack(pop)

unsigned int Crc32Table[256];

unsigned int crc32jam(unsigned char *Block, unsigned int uSize)
{
	unsigned int x = -1; //initial value
	unsigned int c = 0;

	while (c < uSize)
	{
		x = ((x >> 8) ^ Crc32Table[((x ^ Block[c]) & 255)]);
		c++;
	}
	return x;
}

void crc32tab()
{
	unsigned int x, c, b;
	c = 0;

	while (c <= 255)
	{
		x = c;
		b = 0;
		while (b <= 7)
		{
			if ((x & 1) != 0)
				x = ((x >> 1) ^ 0xEDB88320); //polynomial
			else
				x = (x >> 1);
			b++;
		}
		Crc32Table[c] = x;
		c++;
	}
}

bool ReadFile(const char *path, unsigned char **data, unsigned int *dataSize)
{
	FILE *file;
	fopen_s(&file, path, "rb");
	if (!file)
	{
		printf("Error: Could not open file %s for reading.\n", path);
		return 0;
	}

	//Get size of file
	fpos_t fpos;
	fseek(file, 0, SEEK_END);
	fgetpos(file, &fpos);
	fseek(file, 0, SEEK_SET);
	*dataSize = (unsigned int)fpos;

	//Create buffer for new file
	*data = new unsigned char[*dataSize];

	//Read in file
	fread(*data, 1, *dataSize, file);
	fclose(file);
	return 1;
}

bool UnpackSave(header_s *header, unsigned char *data, unsigned char **txtData, unsigned int *l)
{
	*txtData = new unsigned char[header->realSize];
	*l = header->realSize;
	int okay = ezuncompress(*txtData, (long *)l, &data[sizeof(header_s)], (long)header->compressedSize);
	delete[]data;
	if (okay != 0)
	{
		if (okay == EZ_STREAM_ERROR)
			printf("Error: Uncompress failed. Stream error.\n");
		else if (okay == EZ_DATA_ERROR)
			printf("Error: Uncompress failed. Data error.\n");
		else if (okay == EZ_MEM_ERROR)
			printf("Error: Uncompress failed. Memory error.\n");
		else if (okay == EZ_BUF_ERROR)
			printf("Error: Uncompress failed. Buffer error.\n");
		delete[] * txtData;
		return 0;
	}
	return 1;
}

__declspec(dllexport) int Unpack(const char *path)
{
	unsigned char *data = 0;
	unsigned int dataSize = 0;
	if (!ReadFile(path, &data, &dataSize))
		return ERR_READ;

	//Header
	header_s *header = (header_s *)data;
	if (header->u1 != 21)
	{
		printf("Error: Not a valid savegame.\n");
		return ERR_FORMAT;
	}

	//Uncompress data
	unsigned char *txtData = 0;
	unsigned int l = 0;
	if (!UnpackSave(header, data, &txtData, &l))
		return -1;

	//Output text file
	char txtPath[MAXPATH];
	memset(txtPath, 0, MAXPATH);
	sprintf_s(txtPath, MAXPATH, "%s.xml", path);
	FILE *file;
	fopen_s(&file, txtPath, "wb");
	if (!file)
	{
		printf("Error: Could not open file %s for writing.\n", txtPath);
		delete[]txtData;
		return -1;
	}
	fwrite(txtData, 1, l, file);
	fclose(file);

	//Finish
	delete[]txtData;
	printf("Unpacked save to %s\n", txtPath);
	return 0;
}

__declspec(dllexport) int Repack(const char *path)
{
	unsigned char *data = NULL;
	unsigned int dataSize = 0;
	if (!ReadFile(path, &data, &dataSize))
		return ERR_READ;

	//Compress
	unsigned int l = EZ_COMPRESSMAXDESTLENGTH(dataSize);
	unsigned char *compBuffer = new unsigned char[l];
	ezcompress(compBuffer, (long *)&l, data, dataSize);
	delete[]data;

	//Prepare header
	header_s header;
	header.u1 = 21;
	header.u2 = 860693325;
	header.u3 = 0;
	header.u4 = 860700740;
	header.u5 = 1079398965;
	header.compressedSize = l;
	header.realSize = dataSize;
	header.hash = 0;

	//Calculate hash
	crc32tab();
	header.hash = crc32jam(compBuffer, l);

	char *shortPath = _strdup(path);
	//Remove ".txt" from filepath
	{
		int lastDot = 0;
		int pos = 0;
		while (shortPath[pos] != 0)
		{
			if (shortPath[pos] == '.')
				lastDot = pos;
			pos++;
		}
		if (lastDot != 0 && (_stricmp(&shortPath[lastDot], ".txt") == 0 || _stricmp(&shortPath[lastDot], ".xml") == 0))
			shortPath[lastDot] = 0;
	}

	//Create new file
	FILE *file;
	fopen_s(&file, shortPath, "wb");
	if (!file)
	{
		printf("Error: Could not open file %s for writing.\n", shortPath);
		delete[]compBuffer;
		return ERR_WRITE;
	}

	//Write header
	fwrite(&header, sizeof(header_s), 1, file);

	//Write compressed data
	fwrite(compBuffer, l, 1, file);

	//Write padding
	unsigned int paddingSize = SAVESIZE - sizeof(header_s) - l;
	unsigned char *padding = new unsigned char[paddingSize];
	memset(padding, 0, paddingSize);
	fwrite(padding, paddingSize, 1, file);

	//Finish
	fclose(file);
	delete[]compBuffer;
	delete[]padding;
	return 0;
}

__declspec(dllexport) int Validate(const char *path)
{
	unsigned char *data = 0;
	unsigned int dataSize = 0;
	if (!ReadFile(path, &data, &dataSize))
		return ERR_READ;

	//Header
	header_s *header = (header_s *)data;
	if (header->u1 != 21)
	{
		return ERR_FORMAT;
	}

	return 0;
}

extern "C" __declspec(dllexport) int UnpackTest(const char *path, char *unpackedSav)
{
	unpackedSav[0] = 'h';
	unpackedSav[1] = 'i';
	unpackedSav[2] = '\0';
	return 0;
}

