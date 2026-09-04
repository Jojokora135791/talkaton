import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('рисует шапку Толка с вкладкой Календарь', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const header: HTMLElement = fixture.nativeElement;
    expect(header.querySelector('.header__brand')?.textContent).toContain('Контур Толк');
    expect(header.textContent).toContain('Календарь');
  });
});
