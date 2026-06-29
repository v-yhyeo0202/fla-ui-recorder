using System.Collections.Generic;

namespace Shared;

public enum FormType
{
    TextForm,
    ComboForm,
    PathForm,
    IntForm,
    DecimalForm,
    DateForm,
    CheckBoxForm
}

public class FormPropertyConfig
{
    public string displayName = null;
    public FormType? formType = null;
    public List<string> listItem = null;
    public string itemSelectCode = null;
    public int labelWidth = 0;
    public bool bFilePath = true;
    public bool bSave = false;
    public bool bPostSearchPath = false;
}

public class FormConfig
{
    public int labelWidth = 100;
    public bool bAddItem = false;
    public bool bApply = false;
    public string applyConditionCommandText = null;
    public bool bSpecifyConfigPath = false;

    public Dictionary<string, FormPropertyConfig> dictPropertyConfig = null;
}