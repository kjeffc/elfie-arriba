#!/bin/sh
while :;do
   dotnet Arriba.WorkItemCrawler.dll $1 $2
   sleep 300
done