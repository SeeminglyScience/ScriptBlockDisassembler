#
# Module manifest for module 'ScriptBlockDisassembler'
#
# Generated by: Patrick Meinecke
#
# Generated on: 11/13/2021
#

@{

# Script module or binary module file associated with this manifest.
RootModule = 'ScriptBlockDisassembler.dll'

# Version number of this module.
ModuleVersion = '1.0.0'

# ID used to uniquely identify this module
GUID = '32c179e6-6ee8-4ce5-9feb-4962fdb65bb9'

# Author of this module
Author = 'Patrick Meinecke'

# Company or vendor of this module
CompanyName = 'Community'

# Copyright statement for this module
Copyright = '(c) Patrick Meinecke. All rights reserved.'

# Description of the functionality provided by this module
Description = 'Show a C# representation of what the PowerShell compiler generates for a ScriptBlock.'

# Minimum version of the PowerShell engine required by this module
PowerShellVersion = '7.2'

# Functions to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no functions to export.
FunctionsToExport = @()

# Cmdlets to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no cmdlets to export.
CmdletsToExport = 'Get-ScriptBlockDisassembly'

# Variables to export from this module
VariablesToExport = @()

# Aliases to export from this module, for best performance, do not use wildcards and do not delete the entry, use an empty array if there are no aliases to export.
AliasesToExport = @()

# Private data to pass to the module specified in RootModule/ModuleToProcess. This may also contain a PSData hashtable with additional module metadata used by PowerShell.
PrivateData = @{

    PSData = @{
        # Tags applied to this module. These help with module discovery in online galleries.
        Tags = 'disasm', 'linq', 'C#'

        # A URL to the license for this module.
        LicenseUri = 'https://github.com/SeeminglyScience/ScriptBlockDisassembler/blob/master/LICENSE'

        # A URL to the main website for this project.
        ProjectUri = 'https://github.com/SeeminglyScience/ScriptBlockDisassembler'
    } # End of PSData hashtable

} # End of PrivateData hashtable
}