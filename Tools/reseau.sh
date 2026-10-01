#!/bin/sh
# LE BANC D'ESSAI DU RESEAU : un hote et deux invites se parlent sur cette machine
# (Assets/_Fief/Scripts/Net/NetLink.cs, sans Unity). Il faut le SDK .NET.
#     sh Tools/reseau.sh
cd "$(dirname "$0")/reseau" && dotnet run -nologo -v q 2>&1 | grep -v "^$"
