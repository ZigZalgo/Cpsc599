print ('started reading file')

with open('NCDB_1999_to_2015.csv') as f:
	lines = f.readlines()

print ('finished reading file')

headers = 'P_COUNT,' + lines[0]

entries = []

print ('started cleaning data')

currentCount = 0
currentVid = None
currentCid = None
driverLine = None

P_PSN = 18
V_ID = 12
C_CASE = 22

for line in lines:
	columns = line.split(',')
	if (columns[C_CASE] == currentCid):
		if (columns[V_ID] == currentVid):
			currentCount = currentCount + 1
		else:
			if (driverLine is not None):
				entries.append(str(currentCount) + ',' + driverLine)
			driverLine = None
			currentCount = 1
			currentVid = columns[V_ID]
	else:
		if (driverLine is not None):
			entries.append(str(currentCount) + ',' + driverLine)
		driverLine = None
		currentCount = 1
		currentVid = columns[V_ID]
		currentCid = columns[C_CASE]

	if (columns[P_PSN] == '11'):
		driverLine = line

print ('finished cleaning data')

entries.insert(0, headers)

print ('started writing file')

with open('cleaned.csv', 'wb') as f:
	for item in entries:
		f.write("%s" % item)

print ('finished writing file')