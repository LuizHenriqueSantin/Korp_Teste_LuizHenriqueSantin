export interface TableColumn<T> {
  key: string;
  label: string;
  format?: (row: T) => string;
}
