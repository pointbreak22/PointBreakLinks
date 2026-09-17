// Authenticated GET responses need the Bearer token, which a plain `<a href>` navigation can't
// carry — so exports come back as a Blob (SitesApiService.exportWebmasterSales/exportProjectSites)
// and get pushed to disk through a throwaway anchor instead of a real navigation.
export function downloadBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const anchor = document.createElement('a');
  anchor.href = url;
  anchor.download = filename;
  anchor.click();
  URL.revokeObjectURL(url);
}
