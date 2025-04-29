@echo off
del samples\TestPlugin\bin\sample.plx
src\PluginXpert.Cli\bin\Debug\net8.0\PluginXpert.Cli.exe create samples\TestPlugin\bin\sample.plx sample -n Sample
src\PluginXpert.Cli\bin\Debug\net8.0\PluginXpert.Cli.exe add samples\TestPlugin\bin\sample.plx samples\TestPlugin\bin\Debug\net8.0 -c samples\TestPlugin\sample.json