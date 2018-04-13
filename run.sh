#!/bin/bash

data="NCDB.binary"
rules="rules.txt"
out="out.txt"
headers="headers.txt"
search="C_SEV"

parameters=""

while [ "$1" != "" ]; do
	case $1 in
		data )      			shift
								data=$1
								echo "Data file: $data"
								;;
		rules )     			shift
								rules=$1
								echo "Rules file: $rules"
								;;
		out )       			shift
								out=$1
								echo "parameters file: $out"
								;;
		headers )   			shift
								headers=$1
								echo "Headers file: $headers"
								;;
		search )    			shift
								search=$1
								echo "Search column: $search"
								;;
		MaxDepth )      		shift
								echo "MaxDepth: $1"
								parameters="$parameters -MaxDepth $1"
								;;
		MinDivSize )    		shift
								echo "MinDivSize: $1"
								parameters="$parameters -MinDivSize $1"
								;;
		MaxContinuousSplits )	shift
								echo "MaxContinuousSplits: $1"
								parameters="$parameters -MaxContinuousSplits $1"
								;;
		LeafNodeMinimum )		shift
								echo "LeafNodeMinimum: $1"
								parameters="$parameters -LeafNodeMinimum $1"
								;;
	esac
	shift
done

mono C45NCDB/bin/Release/C45NCDB.exe $data $rules $out $headers $search$parameters