import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { WorkflowSearchComponent } from './workflow-search.component';

describe('WorkflowSearchComponent', () => {
  let component: WorkflowSearchComponent;
  let fixture: ComponentFixture<WorkflowSearchComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ WorkflowSearchComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(WorkflowSearchComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
