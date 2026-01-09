export interface Dataset {
    datasetId: string;
    documentId: string;
    title: string;
    previewAbstract: string; // Match your JSON
    authors: string;         // It's a string, not an array
    confidenceScore: number; // Match your JSON
}