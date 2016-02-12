#pragma once

extern "C" __declspec(dllexport) int Unpack(const char *path, char *output);
extern "C" __declspec(dllexport) int Repack(const char *outputPath, const char *xmlData, unsigned int dataSize);

extern "C" __declspec(dllexport) int Validate(const char *path);
