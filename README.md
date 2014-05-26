# Checksum #

### Usage ###

* CheckSum --Create -p PackagePath
* CheckSum --Check -p PackagePath

### Parameters details ###

| -m, --Mode                     | (Default: Check) Run mode (Create,Check,Clear)                      |
| --Create                       | Create checksum files. Existing checksum files are replaced.        |
| --Check                        |  Check existing checksum files                                      |
| --Clear                        |  Remove checksum files                                              |
| -p, --PackagePath              |  (Default: .) Path to package directory                             |
| --ChecksumInCurrentDirectory   |  (Default: False) Checksum files are located in current directory   |
| -d, --DetailedChecksumFileName |  (Default: checksum_detailed.txt) Detailed checksum file            |
| -g, --GlobalChecksumFileName   |  (Default: checksum.txt) Global checksum file                       |
| -v, --Verbosity                |  (Default: Normal) Output verbosity (Silent,Terse,Normal,Verbose)   |
| -t, --PackageType              |  (Default: ZipAndPatch) Package type (ZipAndPatch,FullDirectory)    |
| -a, --Algorithm                |  (Default: MD5) Algorithm name (MD5, SHA1, SHA256...)               |
| --HideProgress                 |  Do not display progress details                                    |
| --DegreeOfParallelism          |  (Default: ) Use n CPU for hash calculation                         |
| --help                         |  Display this help screen.                                          |

### Return values ###
	
* 0 : Success
* 1 : Error
* 2 : Check failed

