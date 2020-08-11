#!/bin/sh
while [ -z ${CRAWLER_DISABLED} ]
do
   dotnet Arriba.WorkItemCrawler.dll configName=default mode=-i
   sleep 300
done