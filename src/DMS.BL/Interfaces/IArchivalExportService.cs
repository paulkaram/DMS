using DMS.BL.DTOs;

namespace DMS.BL.Interfaces;

public interface IArchivalExportService
{
    /// <summary>
    /// Generates an ISO 14721 (OAIS) compliant archival export package for a single document.
    /// Package includes: original file, metadata.json, audit-trail.json, integrity-manifest.json.
    /// </summary>
    Task<ServiceResult<byte[]>> ExportDocumentArchiveAsync(Guid documentId);

    /// <summary>
    /// Generates archival export packages for all documents in a folder.
    /// Returns a ZIP containing individual document packages.
    /// </summary>
    Task<ServiceResult<byte[]>> ExportFolderArchiveAsync(Guid folderId);
}
