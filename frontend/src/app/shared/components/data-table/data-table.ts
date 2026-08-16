import { Component, ContentChild, TemplateRef, input, output } from '@angular/core';
import { MatTableModule } from '@angular/material/table';
import { NgTemplateOutlet } from '@angular/common';
import { TableColumn } from './table-column.model';

@Component({
  selector: 'app-data-table',
  imports: [MatTableModule, NgTemplateOutlet],
  templateUrl: './data-table.html',
  styleUrl: './data-table.scss',
})
export class DataTable<T extends object> {
  columns = input.required<TableColumn<T>[]>();
  data = input.required<T[]>();
  emptyMessage = input('Nenhum registro encontrado.');
  clickavel = input(false);

  rowClick = output<T>();

  @ContentChild('actions', { read: TemplateRef })
  actionsTemplate?: TemplateRef<{ $implicit: T }>;

  protected get displayedColumns(): string[] {
    const keys = this.columns().map((c) => c.key);
    return this.actionsTemplate ? [...keys, 'acoes'] : keys;
  }

  protected valorCelula(row: T, column: TableColumn<T>): string {
    if (column.format) {
      return column.format(row);
    }

    const valor = (row as Record<string, unknown>)[column.key];
    return valor === null || valor === undefined ? '' : String(valor);
  }

  protected aoClicarLinha(row: T): void {
    if (this.clickavel()) {
      this.rowClick.emit(row);
    }
  }
}
