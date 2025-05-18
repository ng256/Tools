# NtfsFileStream

**NtfsFileStream** is a .NET library that provides functionality to work with NTFS file streams. It allows you to access, manipulate, and manage alternate data streams associated with files in the NTFS file system.

## Features

- **Access NTFS Streams**: Read and write to alternate data streams in NTFS.
- **Stream Management**: Create, delete, and check the existence of NTFS streams.
- **Zone Identifier**: Set and remove the transfer zone identifier for files.
- **Asynchronous Operations**: Support for asynchronous file operations.

## Installation

Just add the **NtfsFileStream.cs** file to your project.

### Prerequisites

- .NET Framework or .NET Core compatible with the library.

## Usage

### Basic Usage

Here's how you can use the NtfsFileStream class to work with NTFS streams:

```csharp
using System.IO;

// Open a file stream
using (var stream = NtfsFileStream.Open("path/to/your/file", "StreamName", FileMode.Open, FileAccess.Read))
{
    // Read from the stream
    byte[] buffer = new byte[1024];
    int bytesRead;
    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
    {
        // Process the data
    }
}

// Create a new stream
using (var stream = NtfsFileStream.Create("path/to/your/file", "NewStream", FileOptions.None))
{
    // Write to the stream
    byte[] data = new byte[] { /* your data */ };
    stream.Write(data, 0, data.Length);
}

// Set a Zone Identifier
NtfsFileStream.SetZoneId("path/to/your/file", ZoneId.Internet);

// Remove a Zone Identifier
NtfsFileStream.RemoveZoneId("path/to/your/file");

```

## API Reference
NtfsFileStream
- Open: Opens an existing NTFS stream for reading or writing.
- Create: Creates a new NTFS stream or overwrites an existing one.
- Delete: Deletes a specified NTFS stream.
- Exists: Checks if a specified NTFS stream exists.
- SetZoneId: Sets the transfer zone identifier for a file.
- RemoveZoneId: Removes the transfer zone identifier from a file.


## License
This project is licensed under the following licenses:

- The Code Project Open License (CPOL) version 1 or later.
- The GNU General Public License as published by the Free Software Foundation, version 3 or later.
- The BSD 2-Clause License.
See the license files for details.

## Acknowledgements
This project is based on the Trinet.Core.IO.Ntfs project by Richard Deeming.
