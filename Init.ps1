param($package)
copy "lib\net45\" + $package + ".dll" "C:\Program Files (x86)\Microsoft BizTalk Server 2013 R2\Pipeline Components\" + $package + ".dll"