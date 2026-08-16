import { Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-loading-button',
  imports: [MatButtonModule, MatProgressSpinnerModule],
  templateUrl: './loading-button.html',
  styleUrl: './loading-button.scss',
})
export class LoadingButton {
  label = input.required<string>();
  loading = input(false);
  disabled = input(false);
  color = input<'primary' | 'accent' | 'warn'>('primary');
  type = input<'button' | 'submit'>('button');

  clicked = output<void>();

  protected aoClicar(): void {
    if (!this.loading() && !this.disabled()) {
      this.clicked.emit();
    }
  }
}
