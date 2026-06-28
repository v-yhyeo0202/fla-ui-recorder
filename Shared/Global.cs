using System.Collections.Generic;

namespace Shared;

public static class Global
{
    public static readonly string targetedProject = "RecorderUI";
    public static readonly string sourceGenLogPath = "c:\\fla-ui-recorder\\sourceGen.log";
    public static readonly Dictionary<string, FormConfig> dictFormConfig = new Dictionary<string, FormConfig>
    {
        {
            "RecorderConfig",
            new()
            {
                labelWidth = 100,
                bApply = true,
                dictPropertyConfig = new Dictionary<string, FormPropertyConfig>
                {
                    {
                        "applicationPath",
                        new()
                        {
                            displayName = "Application path",
                            formType = FormType.PathForm
                        }
                    },
                    {
                        "stepDirectoryPath",
                        new()
                        {
                            displayName = "Step directory path",
                            formType = FormType.PathForm,
                            bFilePath = false
                        }
                    }
                }
            }
        }
    };
}