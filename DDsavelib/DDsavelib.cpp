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

int ReadFile(const char *path, unsigned char **data, unsigned int *dataSize)
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

	//Create buffer for new file
	*data = new unsigned char[*dataSize];

	//Read in file
	fread(*data, 1, *dataSize, file);
	fclose(file);
	return 0;
}

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
	
	errcode = UnpackSave(header, data, &output, &l);
	if (errcode)
		return errcode;

	return 0;
}
