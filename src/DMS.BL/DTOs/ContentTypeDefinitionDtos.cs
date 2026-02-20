namespace DMS.BL.DTOs;

public class CreateContentTypeDefinitionRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public string? Category { get; set; }
    public bool AllowOnFolders { get; set; } = true;
    public bool AllowOnDocuments { get; set; } = true;
    public bool IsRequired { get; set; } = false;
    public bool IsSystemDefault { get; set; } = false;
    public Guid? DefaultClassificationId { get; set; }
    public int SortOrder { get; set; } = 0;
}

public class UpdateContentTypeDefinitionRequest : CreateContentTypeDefinitionRequest
{
    public bool IsActive { get; set; } = true;
}

public class CreateFieldRequest
{
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string FieldType { get; set; } = "Text";
    public bool IsRequired { get; set; } = false;
    public bool IsReadOnly { get; set; } = false;
    public bool ShowInList { get; set; } = false;
    public bool IsSearchable { get; set; } = true;
    public string? DefaultValue { get; set; }
    public string? ValidationRules { get; set; }
    public string? LookupName { get; set; }
    public string? Options { get; set; }
    public string? GroupName { get; set; }
    public int ColumnSpan { get; set; } = 12;
}

public class UpdateFieldRequest : CreateFieldRequest
{
    public int SortOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}

public class ReorderFieldsRequest
{
    public List<Guid> FieldIds { get; set; } = new();
}

public class SaveMetadataRequest
{
    public Guid FieldId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? Value { get; set; }
    public decimal? NumericValue { get; set; }
    public DateTime? DateValue { get; set; }
}

public class UpdateAssignmentRequest
{
    public bool IsRequired { get; set; }
    public bool IsDefault { get; set; }
    public bool InheritToChildren { get; set; }
    public int DisplayOrder { get; set; }
}
