import { Component, OnInit, ElementRef, ViewChild } from '@angular/core';
import { DatePipe } from '@angular/common';
import { DataSource, } from '@angular/cdk/collections';
import { MatPaginator } from '@angular/material';
import { BehaviorSubject } from 'rxjs/BehaviorSubject';
import { Observable } from 'rxjs/Observable';
import { ObservableDataSource } from '../../common/observable-datasource';
import 'rxjs/add/operator/startWith';
import 'rxjs/add/observable/merge';
import 'rxjs/add/operator/map';
import 'rxjs/add/operator/debounceTime';
import 'rxjs/add/operator/distinctUntilChanged';
import 'rxjs/add/observable/fromEvent';

@Component({
  selector: 'app-workflow-search',
  templateUrl: './workflow-search.component.html',
  styleUrls: ['./workflow-search.component.css']
})
export class WorkflowSearchComponent implements OnInit {

  displayedColumns = ['createTime', 'definition', 'version', 'reference', 'status', 'actions'];
  dataChange: BehaviorSubject<any[]> = new BehaviorSubject<any[]>([]);
  dataSource: ObservableDataSource<any> | null;
  isLoading = false;

  @ViewChild('search') search: ElementRef;
  @ViewChild('paginator') paginator: MatPaginator;


  constructor() { }

  ngOnInit() {    
    Observable.fromEvent(this.search.nativeElement, 'keyup')
        .debounceTime(150)
        .distinctUntilChanged()
        .subscribe(() => {
          if (!this.dataSource) { return; }
          this.fetchData();
        });

    this.dataSource = new ObservableDataSource(this.dataChange, []);

    this.fetchData();
  }

  async retry(id: string) {
    this.isLoading = true;
    await this.workflowService.retry(id);
    this.isLoading = false;
    this.fetchData();
  }

  async fetchData() {
    let searchStr = this.search.nativeElement.value;
    let skip = this.paginator.pageIndex * this.paginator.pageSize;
    let take = this.paginator.pageSize;
    
    if (isNaN(skip))
      skip = 0;

      if (isNaN(take))
        take = 0;

    this.isLoading = true;
    var data = await this.workflowService.getWorkflows(searchStr, skip, take);    
    this.isLoading = false;
    this.paginator.length = data.Count;    

    var transformed = data.Data.map(input => {
      var output = new WorkflowInstance();
      output.id = input.Id;
      
      return output;
    });

    this.dataChange.next(transformed);
  }

}
