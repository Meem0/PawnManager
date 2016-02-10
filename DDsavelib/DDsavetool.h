#pragma once

extern "C" __declspec(dllexport) int Unpack(const char *path);

extern "C" __declspec(dllexport) int UnpackTest(const char *path, char *unpackedSav);

extern "C" __declspec(dllexport) int Repack(const char *path);

extern "C" __declspec(dllexport) int Validate(const char *path);

extern "C" __declspec(dllexport) int ERR_READ;
extern "C" __declspec(dllexport) int ERR_WRITE;
extern "C" __declspec(dllexport) int ERR_FORMAT;
extern "C" __declspec(dllexport) int ERR_UNPACK;
