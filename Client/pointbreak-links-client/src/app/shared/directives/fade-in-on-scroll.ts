import { Directive, ElementRef, OnDestroy, OnInit, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

// Reusable equivalent of FOXLinks' manual `.fade-in` + scroll-listener pattern (repeated in
// every page's onMounted hook) — an IntersectionObserver instead of a scroll handler, and
// one directive instead of copy-pasted setup per page.
@Directive({ selector: '[appFadeIn]' })
export class FadeInOnScroll implements OnInit, OnDestroy {
  private readonly el = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private observer?: IntersectionObserver;

  ngOnInit(): void {
    if (!this.isBrowser) return;

    const element = this.el.nativeElement;
    element.classList.add('opacity-0', 'translate-y-8', 'transition-all', 'duration-700', 'ease-out');

    this.observer = new IntersectionObserver(
      ([entry]) => {
        if (entry.isIntersecting) {
          element.classList.remove('opacity-0', 'translate-y-8');
          this.observer?.disconnect();
        }
      },
      { threshold: 0, rootMargin: '0px 0px -100px 0px' },
    );
    this.observer.observe(element);
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }
}
