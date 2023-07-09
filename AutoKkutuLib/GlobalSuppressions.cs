// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "CA1031")] // Do not catch general exception types
[assembly: SuppressMessage("Reliability", "CA2007")] // Do not directly await a Task
[assembly: SuppressMessage("Roslynator", "RCS1123")] // Add parentheses when necessary
[assembly: SuppressMessage("Minor Code Smell", "S3220")] // Method calls should not resolve ambiguously to overloads with "params"
/* [assembly: SuppressMessage("Major Securiy Hotspot", "S2077")] // Formatting SQL queries is security-sensitive */

[assembly: SuppressMessage("Major Code Smell", "S107")] // Methods should not have too many parameters

[assembly: SuppressMessage("Security", "CA5394")] // Do not use insecure randomness
[assembly: SuppressMessage("Major Code Smell", "S1121")] // Assignments should not be made from within sub-expressions
