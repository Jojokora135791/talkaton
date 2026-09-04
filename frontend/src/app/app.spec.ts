import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([]), provideHttpClient(), provideHttpClientTesting()],
    }).compileComponents();
  });

  it('рисует шапку Толка с вкладкой Календарь', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const header: HTMLElement = fixture.nativeElement;
    expect(header.querySelector('.header__brand')?.textContent).toContain('Контур Толк');
    expect(header.textContent).toContain('Календарь');
  });

  it('без входа не показывает имя пользователя в шапке', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.header__user')).toBeNull();
  });
});
