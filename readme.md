# CPSC 599.44 Project

## Installation

Build the project using the command `make release`.

## Usage

Run the program using the command `./run.sh`. You may optionally supply the following arguments to this script (in the given order):
 - search: The feature to search optionally				(defaults to C_SEV)
 - data: The file name of the binary data to use		(defaults to NCDB.binary)
 - rules: The file name of the rules file to use		(defaults to rules.txt)
 - headers: The file name of the headers tile to use	(defaults to headers.txt)
 - out: The file name of the output rules file			(defaults to out.txt)

A rules and headers file **must** be passed to the program, but they may be empty files.

The rules file must contain zero or more lines of the format:

	FEATURE OPERATOR VALUE

Where `FEATURE` must be one of the feature names for the dataset (e.g. C_HOUR), `OPERATOR` must be one of `==`, `<`, or `>`, and `VALUE` must be an integer.

The headers file must contain zero or more lines of the format:

	DIRECTIVE FEATURE

Where `FEATURE` must be one of the feature names for the dataset (e.g. C_HOUR), and `DIRECTIVE` must be one of `cont` or `ignore`. If the `cont` directive is used, then the feature will be considered continuous during learning. If the `ignore` directive is used, then the feature will be completely ignored during learning.

## Data Binary

As described above, a data binary file must be passed to the learner. This file is generated using a C# program from a CSV downloaded from the [National Collision Database of Canada](https://open.canada.ca/data/en/dataset/1eb9eba7-71d1-4b30-9fb1-30cbdab7e63a).

Building the C# program is a bit complicated. Open the C45NCDB/C45NCDB.csproj file. On line 50, replace "Program" with "CodifyInput". Now, run `make release`.

Once built, you can run the codifier using the command `mono C45NCDB/bin/Release/C45NCDB.exe DATA_FILE` where `DATA_FILE` is the file path to the CSV you want to codify.

This will output a file called NCDB.binary, which can be used as input for the learner.

Remember to undo your changes to the C45NCDB/C45NCDB.csproj file and to rebuild the project before trying to run the learner.

## Data Cleaning

Before generating the data binary, there is a cleaner script which can be run on the CSV from the NCDB website. This can be run with the command `python cleaner.py DATA_FILE` where `DATA_FILE` is the file path to the CSV downloaded from the NCDB website linked above. This will output a cleaned.csv file which can be used as input to the codify input program described above.

## Statistics

There is a simple Java program which can be used to generate some basic statistics on the data set. We recommend running this on the cleaned data and to use it as reference when looking at the results of learning.

You can build the program with the command `javac StatsCalc.java` and you can run the program with `java StatsCalc DATA_FILE` where `DATA_FILE` is the file path the CSV you want to generate statistics on. This will output a stats.csv file which can be viewed in your data viewer of choice.
