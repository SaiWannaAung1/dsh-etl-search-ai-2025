import { BaseService } from './BaseService';
import type { DatasetDetail } from '$lib/models/DatasetDetail';

export class DatasetDetailService extends BaseService {
    /**
     * Fetches full metadata for a specific dataset by its Guid
     * Appends the ID to the /Datasets endpoint
     */
    async getDatasetById(id: string): Promise<DatasetDetail> {
        return await this.get<DatasetDetail>(`/Search/${id}`);
    }
}

export const datasetDetailService = new DatasetDetailService();