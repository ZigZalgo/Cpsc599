#!/bin/bash

search=${1:-C_SEV}
data=${2:-NCDB.binary}
rules=${3:-rules.txt}
headers=${4:-headers.txt}
out=${5:-out.txt}

echo "Search column: $search"
echo "Data file: $data"
echo "Rules file: $rules"
echo "Headers file: $headers"
echo "Output file: $out"

mono C45NCDB/bin/Release/C45NCDB.exe $data $rules $out $headers $search
