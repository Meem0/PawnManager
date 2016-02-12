// DDsavetool.cpp : Defines the exported functions for the DLL application.
//

#include "DDsavelib.h"

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

int ERR_READ = 1;
int ERR_WRITE = 2;
int ERR_FORMAT = 3;
int ERR_UNPACK = 4;
int ERR_STREAM = EZ_STREAM_ERROR;
int ERR_DATA = EZ_DATA_ERROR;
int ERR_MEMORY = EZ_MEM_ERROR;
int ERR_BUFFER = EZ_BUF_ERROR;

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

// if maxRead == 0, reads to end of file
int ReadFile(const char *path, unsigned char **data, unsigned int *dataSize, unsigned int maxRead = 0)
{
	FILE *file;
	fopen_s(&file, path, "rb");
	if (!file)
	{
		printf("Error: Could not open file %s for reading.\n", path);
		return ERR_READ;
	}

	//Get size of file
	fpos_t fpos;
	fseek(file, 0, SEEK_END);
	fgetpos(file, &fpos);
	fseek(file, 0, SEEK_SET);
	*dataSize = (unsigned int)fpos;

	if (maxRead > 0 && maxRead < *dataSize)
	{
		*dataSize = maxRead;
	}

	//Create buffer for new file
	*data = new unsigned char[*dataSize];

	//Read in file
	fread(*data, 1, *dataSize, file);
	fclose(file);
	return 0;
}




//////////////////////////////////////////////////////////////////////////
// old stuff






bool UnpackSaveOld(header_s *header, unsigned char *data, unsigned char **txtData, unsigned int *l)
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

int UnpackOld(const char *path, unsigned char **outputTextData)
{
	unsigned char *data = 0;
	unsigned int dataSize = 0;
	if (ReadFile(path, &data, &dataSize))
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
	if (!UnpackSaveOld(header, data, &txtData, &l))
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

	*outputTextData = txtData;

	//Finish
	//delete[]txtData;
	printf("Unpacked save to %s\n", txtPath);
	return 0;
}









//////////////////////////////////////////////////////////////////////////
// end old stuff










int UnpackSave(header_s *header, unsigned char *data, unsigned char **txtData, unsigned int *l)
{
	*l = header->realSize;
	int errcode = ezuncompress(*txtData, (long *)l, &data[sizeof(header_s)], (long)header->compressedSize);
	delete[]data;
	if (errcode)
	{
		return errcode;
	}
	return 0;
}

__declspec(dllexport) int Unpack(const char *path, char *output)
{
	unsigned char *oldOutput = nullptr;
	UnpackOld(path, &oldOutput);


	unsigned char *data = 0;
	unsigned int dataSize = 0;
	int errcode = ReadFile(path, &data, &dataSize);
	if (errcode)
		return errcode;

	//Header
	header_s *header = (header_s *)data;
	if (header->u1 != 21)
	{
		return ERR_FORMAT;
	}

	//Uncompress data
	unsigned int l = 0;

	unsigned char *unpackedText = reinterpret_cast<unsigned char *>(output);

	errcode = UnpackSave(header, data, &unpackedText, &l);
	if (errcode)
		return errcode;

	for (unsigned int i = 0; i < l; ++i)
	{
		if (oldOutput[i] != unpackedText[i])
		{
			printf("woh\n");
		}
	}

	//Output text file
	char txtPath[MAXPATH];
	memset(txtPath, 0, MAXPATH);
	sprintf_s(txtPath, MAXPATH, "%s_dll.xml", path);
	FILE *file;
	fopen_s(&file, txtPath, "wb");
	if (!file)
	{
		printf("Error: Could not open file %s for writing.\n", txtPath);
		return -1;
	}
	fwrite(unpackedText, 1, l, file);
	fclose(file);
	printf("Unpacked DLL TEST save to %s\n", txtPath);

	delete[] oldOutput;

	return 0;
}

__declspec(dllexport) int Repack(const char *outputPath, const char *xmlData, unsigned int dataSize)
{
	const unsigned char *data = reinterpret_cast<const unsigned char *>(xmlData);

	//Compress
	unsigned int l = EZ_COMPRESSMAXDESTLENGTH(dataSize);
	unsigned char *compBuffer = new unsigned char[l];
	ezcompress(compBuffer, (long *)&l, data, dataSize);

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

	//Create new file
	FILE *file;
	fopen_s(&file, outputPath, "wb");
	if (!file)
	{
		printf("Error: Could not open file %s for writing.\n", outputPath);
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
	int errcode = ReadFile(path, &data, &dataSize, 64);

	//Header
	header_s *header = (header_s *)data;
	if (header->u1 != 21)
	{
		return ERR_FORMAT;
	}
	else return 0;
}