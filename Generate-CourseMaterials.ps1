if (!(Test-Path .\.materials)) {
    mkdir .\.materials
}

$CommonPaths = @(
    # Add additional shared paths for each module
    ".\LICENSE",
    ".\README.md"
)

Compress-Archive -DestinationPath .\.materials\demo-final.zip -Update -Path ($CommonPaths + @(
    ".\demo-final"
))
Compress-Archive -DestinationPath .\.materials\demo-m1.zip -Update -Path ($CommonPaths + @(
    ".\demo-m1"
))
Compress-Archive -DestinationPath .\.materials\demo-m2.zip -Update -Path ($CommonPaths + @(
    ".\demo-m2"
))
Compress-Archive -DestinationPath .\.materials\demo-m3.zip -Update -Path ($CommonPaths + @(
    ".\demo-m3"
))
Compress-Archive -DestinationPath .\.materials\demo-m4.zip -Update -Path ($CommonPaths + @(
    ".\demo-m4",
    ".\token-counting"
))