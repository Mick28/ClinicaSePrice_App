#!/usr/bin/env bash
# Compilación de verificación en Linux con Mono (no hace falta en Windows:
# allí se abre ClinicaSePrice.sln con Visual Studio 2022).
# Usa el compilador Roslyn del SDK de .NET contra las bibliotecas de Mono
# y SQLite de Mono (Mono.Data.Sqlite), con el símbolo MONO.
#   herramientas/compilar_mono.sh            -> bin_mono/SePrice.exe y Pruebas.exe
set -e
cd "$(dirname "$0")/.."
M=/usr/lib/mono/4.8-api; G=/usr/lib/mono/4.5
CSC="dotnet $(ls -d /usr/lib/dotnet/sdk/*/Roslyn/bincore | head -1)/csc.dll"
REFS="-r:$M/mscorlib.dll -r:$M/System.dll -r:$M/System.Core.dll \
 -r:$M/System.Data.dll -r:$M/System.Drawing.dll -r:$M/System.Xml.dll \
 -r:$M/Facades/netstandard.dll -r:$G/Mono.Data.Sqlite.dll"
RES="-resource:SePrice.Datos/Sql/esquema.sql,SePrice.Datos.Sql.esquema.sql \
 -resource:SePrice.Datos/Sql/datos_ejemplo.sql,SePrice.Datos.Sql.datos_ejemplo.sql"
OPC="-nologo -nostdlib -noconfig -langversion:latest -define:MONO -nowarn:1591"
mkdir -p bin_mono
LIB=$(find SePrice.Datos SePrice.Negocio -name '*.cs' -not -path '*/obj/*')
$CSC $OPC -out:bin_mono/Pruebas.exe $REFS $RES $LIB \
  $(find SePrice.Pruebas -name '*.cs' -not -path '*/obj/*')
if [ -d SePrice.UI ] && ls SePrice.UI/*.cs >/dev/null 2>&1; then
  $CSC $OPC -target:winexe -out:bin_mono/SePrice.exe $REFS \
    -r:$M/System.Windows.Forms.dll $RES $LIB \
    $(find SePrice.UI -name '*.cs' -not -path '*/obj/*')
  cp -f SePrice.UI/Recursos/logo.png SePrice.UI/SePrice.config.txt bin_mono/
fi
echo "Compilación Mono: OK"
