export interface InsightWithAction {
    id: number;
    title: string;
    diagnosis: string;
    icon: string;
    severity: 'success' | 'warning' | 'danger' | 'info';
    priority: 'high' | 'medium' | 'low';
    action?: {
        title: string;
        description: string;
    };
    target?: string;
}