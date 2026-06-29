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
                labelWidth = 150,
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
                        "attacherConfigPath",
                        new()
                        {
                            displayName = "Attacher configuration path",
                            formType = FormType.PathForm,
                            bPostSearchPath = true
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
                    },
                    {
                        "stepName",
                        new()
                        {
                            displayName = "Step name",
                            formType = FormType.TextForm
                        }
                    }
                }
            }
        },
        {
            "AttacherConfig",
            new()
            {
                labelWidth = 150,
                bAddItem = true,
                bApply = true,
                bSpecifyConfigPath = true,
                dictPropertyConfig = new Dictionary<string, FormPropertyConfig>
                {
                    {
                        "mainWindowTitle",
                        new()
                        {
                            displayName = "Main window title",
                            formType = FormType.TextForm
                        }
                    },
                    {
                        "bExactMatch",
                        new()
                        {
                            displayName = "Exact match",
                            formType = FormType.CheckBoxForm
                        }
                    }
                }
            }
        }
    };
}