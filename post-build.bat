D:
cd "D:\applications\xornent\visual studio\VC\Auxiliary\Build"
call vcvarsall.bat x86
cd "D:\projects\xornent\archiver\7z-x86\cpp\7zip\bundles\format7zf"
nmake
cd "D:\projects\xornent\archiver\7z-x86\cpp\7zip\ui\console"
nmake
cd "D:\applications\xornent\visual studio\VC\Auxiliary\Build"
call vcvarsall.bat x64
cd "D:\projects\xornent\archiver\7z-x86\cpp\7zip\bundles\format7zf"
nmake
cd "D:\projects\xornent\archiver\7z-x86\cpp\7zip\ui\console"
nmake
rmdir /s "D:\projects\xornent\archiver\Archiver\bin\Debug\7z\x86" /q
rmdir /s "D:\projects\xornent\archiver\Archiver\bin\Debug\7z\x64" /q
mkdir "D:\projects\xornent\archiver\Archiver\bin\Debug\7z\x86"
mkdir "D:\projects\xornent\archiver\Archiver\bin\Debug\7z\x64"
copy "D:\projects\xornent\archiver\7z-x86\cpp\7zip\bundles\format7zf\x86\7z.dll" "D:\projects\xornent\archiver\Archiver\bin\Debug\7z\x86\7z.dll"
copy "D:\projects\xornent\archiver\7z-x86\cpp\7zip\bundles\format7zf\x64\7z.dll" "D:\projects\xornent\archiver\Archiver\bin\Debug\7z\x64\7z.dll"
copy "D:\projects\xornent\archiver\7z-x86\cpp\7zip\ui\console\x86\7z.exe" "D:\projects\xornent\archiver\Archiver\bin\Debug\7z\x86\7z.exe"
copy "D:\projects\xornent\archiver\7z-x86\cpp\7zip\ui\console\x64\7z.exe" "D:\projects\xornent\archiver\Archiver\bin\Debug\7z\x64\7z.exe"