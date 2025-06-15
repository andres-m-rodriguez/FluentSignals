#!/bin/bash

# Bash script to build NuGet packages locally

# Default values
CONFIGURATION="Release"
NO_BUILD=false
PUSH=false
API_KEY=""
SOURCE="https://api.nuget.org/v3/index.json"

# Color codes
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
WHITE='\033[1;37m'
NC='\033[0m' # No Color

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        --no-build)
            NO_BUILD=true
            shift
            ;;
        --push)
            PUSH=true
            shift
            ;;
        --api-key)
            API_KEY="$2"
            shift 2
            ;;
        --source)
            SOURCE="$2"
            shift 2
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

echo -e "${CYAN}FluentSignals Package Builder${NC}"
echo -e "${CYAN}=============================${NC}"

# Clean previous packages
echo -e "\n${YELLOW}Cleaning previous packages...${NC}"
rm -f ./packages/*.nupkg 2>/dev/null
rm -f ./packages/*.snupkg 2>/dev/null

# Build solution if not skipped
if [ "$NO_BUILD" = false ]; then
    echo -e "\n${YELLOW}Building solution...${NC}"
    dotnet build --configuration $CONFIGURATION
    if [ $? -ne 0 ]; then
        echo -e "${RED}Build failed!${NC}"
        exit 1
    fi
fi

# Create packages directory
mkdir -p ./packages

# Pack FluentSignals
echo -e "\n${YELLOW}Packing FluentSignals...${NC}"
dotnet pack ./FluentSignals/FluentSignals.csproj \
    --configuration $CONFIGURATION \
    --output ./packages \
    --no-build

# Pack FluentSignals.Blazor
echo -e "\n${YELLOW}Packing FluentSignals.Blazor...${NC}"
dotnet pack ./FluentSignals.Blazor/FluentSignals.Blazor.csproj \
    --configuration $CONFIGURATION \
    --output ./packages \
    --no-build

# List created packages
echo -e "\n${GREEN}Created packages:${NC}"
for package in ./packages/*.nupkg; do
    if [ -f "$package" ]; then
        echo -e "  - ${WHITE}$(basename "$package")${NC}"
    fi
done

# Push to NuGet if requested
if [ "$PUSH" = true ]; then
    if [ -z "$API_KEY" ]; then
        echo -e "\n${RED}Error: API key is required for pushing packages${NC}"
        exit 1
    fi

    echo -e "\n${YELLOW}Pushing packages to $SOURCE...${NC}"
    
    for package in ./packages/*.nupkg; do
        if [ -f "$package" ]; then
            echo -e "  ${WHITE}Pushing $(basename "$package")...${NC}"
            dotnet nuget push "$package" \
                --api-key "$API_KEY" \
                --source "$SOURCE" \
                --skip-duplicate
        fi
    done
    
    echo -e "\n${GREEN}Packages pushed successfully!${NC}"
else
    echo -e "\n${GREEN}Packages created in ./packages/${NC}"
    echo -e "${CYAN}To push to NuGet, run:${NC}"
    echo -e "  ${WHITE}./build-packages.sh --push --api-key YOUR_API_KEY${NC}"
fi

echo -e "\n${GREEN}Done!${NC}"