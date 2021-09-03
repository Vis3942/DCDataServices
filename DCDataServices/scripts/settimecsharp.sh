#!/bin/bash

echo "Setting DC System Time"
echo $1
echo $2

date '+%Y-%m-%d' -s $1 
date +%T -s $2
