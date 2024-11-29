build: FORCE
	dotnet restore
	dotnet build

clean: FORCE
	rm -rf */obj */bin
	rm -rf ${USERPROFILE}/.nuget/packages/Dasblinkenlight.StringTemplate4
	rm -f *.nupkg *.snupkg
	rm -rf nuget.config

publish:
	dotnet nuget push Antlr4.StringTemplate/bin/Debug/Dasblinkenlight.StringTemplate4.4.4.0.nupkg --api-key ${trashkey} --source https://api.nuget.org/v3/index.json

FORCE: ; 
