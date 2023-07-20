using System;
using System.ComponentModel.DataAnnotations;
using System.Web;

public class ZipFileAttribute : ValidationAttribute
{
    public override bool IsValid(object value)
    {
        if (value is HttpPostedFileBase file)
        {
            // Check if the file has a .zip extension
            if (!file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        // If the value is not a file or doesn't have a .zip extension, return true (valid)
        return true;
    }
}