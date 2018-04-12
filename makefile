CC=xbuild
SLN=C45NCDB.sln
OUTDIR=C45NCDB/bin/
RUN=run.sh

clean:
	rm -r $(OUTDIR)

release:
	$(CC) /p:Configuration=Release $(SLN)

debug:
	$(CC) /p:Configuration=Debug $(SLN)

all: release debug

run: release
	./$(RUN)
