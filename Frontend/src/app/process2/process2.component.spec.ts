import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Process2Component } from './process2.component';
import { HttpClientTestingModule } from '@angular/common/http/testing';

describe('Process2Component', () => {
  let component: Process2Component;
  let fixture: ComponentFixture<Process2Component>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Process2Component, HttpClientTestingModule]
    })
    .compileComponents();

    fixture = TestBed.createComponent(Process2Component);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
