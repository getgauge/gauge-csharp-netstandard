#!/bin/bash

function checkCommand() {
    command -v $1 >/dev/null 2>&1 || { echo >&2 "$1 is not installed, aborting."; exit 1; }
}

function build() {
    checkCommand "dotnet"
    dotnet build -c release
}

function test() {
    checkCommand "dotnet"
    dotnet test --no-build -c release test/Gauge.Dotnet.UnitTests.csproj
    dotnet test --no-build -c release integration-test/Gauge.Dotnet.IntegrationTests.csproj
}

function version() {
    checkCommand "jq"
    echo `cat src/dotnet.json | jq -r .version`
}

function package() {
    checkCommand "dotnet"
    checkCommand "zip"
    rm -rf deploy artifacts
    dotnet publish -c release -o ../deploy/bin src/Gauge.Dotnet.csproj
    cp src/launcher.sh deploy
    cp src/launcher.cmd deploy
    cp src/dotnet.json deploy
    mkdir -p artifacts
    (export version=$(version) && cd deploy && zip -r ../artifacts/gauge-dotnet-$version.zip .)
}

function install() {
    package
    gauge install dotnet -f ./artifacts/gauge-dotnet-$(version).zip
}

function uninstall() {
    gauge uninstall dotnet -v $(version)
}

function forceinstall() {
    uninstall
    install
}

tasks=(build test package install uninstall forceinstall)
if [[ " ${tasks[@]} " =~ " $1 " ]]; then
    $1
    exit 0
fi

echo Options: [build \| test \| package \| install \| uninstall \| forceinstall]

