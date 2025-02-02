# Description.
Assembly info generator with automatic version increase.  

The file **AssemblyInfo.tt** is a T4 file template for C# projects. Just add it to your C# project and fill in the required variables with your data, and you will get the generated **AsemblyInfo.cs** file in the output. Automatic build version increase occurs each time you click on "Transform T4 Templates".  

Futures:  
- creation of a project description;  
- automatic increment of the assembly version "major.minor.**date.buid**", where:
    - **major** - user-defined majorVersion variable,
    - **minor** - user-defined minorVersion variable, 
    - **date** - today's date in "YYMM" format,
    - **buid** - autoincremented four-digit variable.
- license text (you can replace according to your choice or remove it).  

Here is an example of the generated AssemblyInfo.cs in my "Initialization Settings Library" project.  

```csharp
/*********************************************************
Initialization Settings Library v. 1.0

The MIT License (MIT)
Copyright: © NG256 2021.

Permission is  hereby granted, free of charge, to any person
obtaining   a copy    of    this  software    and associated
documentation  files  (the "Software"),    to  deal   in the
Software without  restriction, including without  limitation
the rights to use, copy, modify, merge, publish, distribute,
sublicense,  and/or  sell  copies   of  the Software, and to
permit persons to whom the Software  is furnished to  do so,
subject       to         the      following      conditions:

The above copyright  notice and this permission notice shall
be  included  in all copies   or substantial portions of the
Software.

THE  SOFTWARE IS  PROVIDED  "AS IS", WITHOUT WARRANTY OF ANY
KIND, EXPRESS  OR IMPLIED, INCLUDING  BUT NOT LIMITED TO THE
WARRANTIES  OF MERCHANTABILITY, FITNESS    FOR A  PARTICULAR
PURPOSE AND NONINFRINGEMENT. IN  NO EVENT SHALL  THE AUTHORS
OR  COPYRIGHT HOLDERS  BE  LIABLE FOR ANY CLAIM,  DAMAGES OR
OTHER LIABILITY,  WHETHER IN AN  ACTION OF CONTRACT, TORT OR
OTHERWISE, ARISING FROM, OUT OF   OR IN CONNECTION  WITH THE
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE. 
***********************************************************/

#if COMVISIBLE
using System.EnterpriseServices;
#endif
using System.Reflection;
using System.Runtime.InteropServices;

// General information about this assembly is provided by the following set
// attributes. Change the values of these attributes to change the information,
// related to the assembly.
[assembly: AssemblyTitle ("Initialization Settings Library 1.0")] // Assembly name.
[assembly: AssemblyDescription ("Initialization Settings Library 1.0")] // Assembly description.
[assembly: AssemblyCompany ("NG256")] // Developer.
[assembly: AssemblyProduct ("NG256 Initialization Settings Library")] // Product name.
[assembly: AssemblyCopyright ("© NG256 2021")] // Copyright.
//[assembly: AssemblyTrademark ("NG256® Initialization Settings Library®")] // Trademark.
[assembly: AssemblyCulture ("")]
[assembly: AssemblyVersion ("1.0.2110.0047")]
[assembly: AssemblyFileVersion ("1.0.2110.0047")]
#if DEBUG
[assembly: AssemblyConfiguration ("Debug")]
#else
[assembly: AssemblyConfiguration ("Release")]
#endif

// Setting ComVisible to False makes the types in this assembly invisible
// for COM components. If you need to refer to the type in this assembly via COM,
// set the ComVisible attribute to TRUE for this type.
#if COMVISIBLE
[assembly: ComVisible (true)]
[assembly: ApplicationName ("IniLib")] // The name of the COM application.
[assembly: ApplicationID ("fc24620a-239d-4e40-b756-7ed38e82ef69")]
#else
[assembly: ComVisible (false)]
#endif
// The following GUID is used to identify the type library if this project will be visible to COM
[assembly: Guid ("e60d1ecf-6c7b-4c9b-925f-4bf07615da87")]
```
