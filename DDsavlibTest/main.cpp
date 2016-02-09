#include <DDsavetool.h>
#include <iostream>

void UnpackTest(const char* file)
{
	int code = Unpack(file);
	std::cout << "Unpacked " << file << ", got code " << code;
}

void RepackTest(const char* file)
{
	int code = Repack(file);
	std::cout << "Repacked " << file << ", got code " << code;
}

void ValidateTest(const char* file)
{
	int code = Validate(file);
	std::cout << "Validated " << file << ", got code " << code;
}

int main(int argc, const char** argv)
{
	std::cout << argc << std::endl;
	for (int i = 0; i < argc; ++i)
	{
		std::cout << argv[i] << std::endl;
	}

	if (argc >= 3)
	{
		char modeArg = argv[1][0];
		switch (modeArg)
		{
		case 'u': UnpackTest(argv[2]); break;
		case 'r': RepackTest(argv[2]); break;
		case 'v': ValidateTest(argv[2]); break;
		}
	}

	int end;
	std::cin >> end;
}
