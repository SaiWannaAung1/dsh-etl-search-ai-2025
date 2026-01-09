export interface DatasetDetail {
    datasetId: string;
    documentId: string;
    title: string;
    abstract: string;
    authors: string;
    keywords: string | null;
    resourceUrl: string | null;
    publishedDate: string | null;
    ingestedAt: string;
}