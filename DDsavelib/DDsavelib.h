#pragma once

extern "C" __declspec(dllexport) int Unpack(const char *path, unsigned char *output);

extern "C" __declspec(dllexport) int ERR_READ;
extern "C" __declspec(dllexport) int ERR_WRITE;
extern "C" __declspec(dllexport) int ERR_FORMAT;
extern "C" __declspec(dllexport) int ERR_UNPACK;
extern "C" __declspec(dllexport) int ERR_STREAM;
extern "C" __declspec(dllexport) int ERR_DATA;
extern "C" __declspec(dllexport) int ERR_MEMORY;
extern "C" __declspec(dllexport) int ERR_BUFFER;
