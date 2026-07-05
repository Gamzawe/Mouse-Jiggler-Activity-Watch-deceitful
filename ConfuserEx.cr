<?xml version="1.0" encoding="utf-8"?>
<project baseDir="." outputDir=".\Confused" xmlns="http://confuser.codeplex.com">
  <rule pattern="true" inherit="false">
    <protection id="anti debug" />
    <protection id="anti dump" />
    <protection id="anti ildasm" />
    <protection id="ctrl flow" />
    <protection id="rename" />
  </rule>
  <module path="LogiOptions.exe" />
</project>
