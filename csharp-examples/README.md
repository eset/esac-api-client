# ESET Secure Authentication Cloud API Client: C# Sample Code

This project is a sample C# application demonstrating the usage of the ESET Secure Authentication Cloud API Client.

## Adding the SDK library

The ESA Cloud C# API Client library is distributed as a [NuGet](https://www.nuget.org) package. In order to use the API Client,
configure the ESA package source and download the library.

1. Configure the ESA package source

   1. In Visual Studio, open the "Tools" menu and select "Options..."
   2. Navigate to "NuGet Package Manager" -> "Package Sources"
   3. Click the "+" icon in the top right of the window and fill in the details at the bottom:
      - Name: ESET Secure Authentication Cloud
      - Source: https://esa.eset.com/sdk/packages/nuget
   4. Click "Update" and then "OK" to close the dialogs

2. Download the library:

   1. In the Visual Studio Solution Explorer, right-click the solution (ESA.API.Examples)
      and select "Manage NuGet packages for solution..."
   2. In the NuGet package window, expand the "Online" -> "ESET Secure Authentication Cloud" source
   3. Find the "ESA.API" package and click the "Install" button
   4. Select the checkbox next to the projects and click "OK"
   5. Read and accept the API Client license
   6. After installation, close the NuGet window

3. Build & run the solution in Visual Studio

## Updating the API Client library

As new versions of the API Client are released, they will show up in the NuGet window under "Updates". Try to always run the latest version.
