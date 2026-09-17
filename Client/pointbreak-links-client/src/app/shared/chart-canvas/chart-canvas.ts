import { AfterViewInit, ChangeDetectionStrategy, Component, ElementRef, OnDestroy, effect, input, viewChild } from '@angular/core';
import { Chart, ChartConfiguration, registerables } from 'chart.js';

Chart.register(...registerables);

// Thin reusable wrapper so pages/analytics/analytics.ts (and any future chart) just hands over
// a ChartConfiguration instead of managing a Chart.js instance's lifecycle itself. Real charts
// backed by real data (see Application/CQRS/Analytics) — unlike FOXLinks' analytics.vue, which
// wires the same library to hardcoded arrays.
@Component({
  selector: 'app-chart-canvas',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `<canvas #canvas></canvas>`,
})
export class ChartCanvas implements AfterViewInit, OnDestroy {
  private readonly canvasRef = viewChild.required<ElementRef<HTMLCanvasElement>>('canvas');
  readonly config = input.required<ChartConfiguration>();

  private chart: Chart | null = null;

  constructor() {
    effect(() => {
      const config = this.config();
      if (!this.chart) return;
      this.chart.data = config.data;
      this.chart.options = config.options ?? {};
      this.chart.update();
    });
  }

  ngAfterViewInit(): void {
    this.chart = new Chart(this.canvasRef().nativeElement, this.config());
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
  }
}
