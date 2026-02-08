namespace DMS.BL.DTOs;

public class PagedResultDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

public class TreeNodeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public bool HasChildren { get; set; }
    public bool IsExpanded { get; set; }
    public List<TreeNodeDto> Children { get; set; } = new();
}

public class LookupItemDto
{
    public Guid Id { get; set; }
    public string Value { get; set; } = string.Empty;
    public string? DisplayText { get; set; }
}

public class ClassificationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class ImportanceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public string? Color { get; set; }
}

public class DocumentTypeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class ServiceResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ServiceResult Ok(string? message = null) => new() { Success = true, Message = message };
    public static ServiceResult Fail(string error) => new() { Success = false, Errors = new List<string> { error } };
    public static ServiceResult Fail(List<string> errors) => new() { Success = false, Errors = errors };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; set; }

    public static ServiceResult<T> Ok(T data, string? message = null) => new() { Success = true, Data = data, Message = message };
    public new static ServiceResult<T> Fail(string error) => new() { Success = false, Errors = new List<string> { error } };
    public new static ServiceResult<T> Fail(List<string> errors) => new() { Success = false, Errors = errors };
}
