# create local package (clp)
param(
    [Parameter(Mandatory=$true)]
    [string]$publishPath,

    [Parameter(Mandatory=$true)]
    [ValidateSet("windows", "linux")]
    [string]$platform,

    [Parameter(Mandatory=$true)]
    [string]$version
)

# Convert publishPath to an absolute path
$publishPath = Resolve-Path $publishPath

# Check if the publishPath is an existing directory
if (!(Test-Path -Path $publishPath -PathType Container)) {
    Write-Host "The provided $$publishPath parameter does not exist. Exiting..."
    exit 1
}

# Check if the version parameter is empty
if ([string]::IsNullOrEmpty($version)) {
    Write-Host "The provided $$version parameter is empty. Exiting..."
    exit 1
}

# Set the name file name of the main app
$platform = $platform.ToLower()
switch ($platform) {
    "linux" {
        $mainAppFile = "BrowserSearch"
    }
    "windows" {
        $mainAppFile = "BrowserSearch.exe"
    }
    default {
        echo "The provided $$platform parameter is invalid. Exiting ..."
    }
}

# Check if the required files and directories exist
if (!(Test-Path -Path (Join-Path -Path $publishPath -ChildPath $mainAppFile)))
{
    Write-Host "$mainAppFile is missing in $publishPath. Exiting..."
    exit 1
}

# Set publishName to the last part of the publishPath
$publishName = (Split-Path -Path $publishPath -Leaf).ToLower()

# Get the current working directory
$currentDirectory = Get-Location

# Create a zip file
switch ($platform) {
    "linux" {
        # Define the tar.gz archive file path
        $archiveFilePath = Join-Path -Path $currentDirectory -ChildPath "BrowserSearch.$publishName.$version.tar.gz"

        # Change the current directory to the directory of the files (so we get relative paths in the gz file)
        Set-Location -Path $publishPath

        # Define the files and directories to be included in the tar.gz archive
        $files = @($mainAppFile, '*.txt', 'appsettings.json')

        # Create the tar.gz file with relative paths
        tar -cvzf $archiveFilePath $files  2>&1 > $null
    }
    "windows" {
        # Zip using the native PowerShell Compress-Archive command
        $archiveFilePath = Join-Path -Path $currentDirectory -ChildPath "BrowserSearch.$publishName.$version.zip"
        Compress-Archive -Path (Join-Path -Path $publishPath -ChildPath *.*) -Force -DestinationPath $archiveFilePath  2>&1 > $null
    }
}


# Change the current directory back to the original directory
Set-Location -Path $currentDirectory

Write-Host "Local package file created at $archiveFilePath"

exit 0
