export interface Goal {
    id: number;
    title: string;
    description: string;
    progress: number;
    icon: string;
    detail?: string;
    targetValue?: number;
    targetLabel?: string;
}