# Quick Start Guide - Analytics & Graphs

## ?? Getting Started (5 Minutes)

### Step 1: Verify Installation
All necessary files have been created. Run the application:

```bash
# From the solution directory
dotnet run --project FiberHelp
```

### Step 2: Login as Administrator
1. Open the application in your browser
2. Login with admin credentials:
   - **Email**: admin@fiberhelp.com
   - **Password**: Adminlogin123@

### Step 3: Navigate to Analytics
Two ways to access:
1. Click **"Analytics"** in the left sidebar (with chart icon ??)
2. Click **"?? View Analytics"** button on the Dashboard

### Step 4: Explore the Features

#### View Key Metrics
At the top, you'll see 4 colored cards showing:
- ?? Total Revenue (Blue)
- ? Resolution Rate (Green)
- ?? Avg Response Time (Orange)
- ?? Client Growth (Purple)

Each shows current value and trend percentage.

#### Interact with Charts
7 interactive charts are displayed:

1. **Ticket Trends** (Full width)
   - Switch between Line/Bar/Area using buttons
   - Hover over data points for details

2. **Revenue vs Expenses** (Left column)
   - Monthly comparison
   - Hover for exact amounts

3. **Client Growth** (Right column)
   - New clients over time
   - Area chart visualization

4. **Priority Distribution** (Left column)
   - Doughnut chart
   - Shows Low/Medium/High/Critical breakdown

5. **Status Comparison** (Right column)
   - Current vs Previous period
   - Grouped bars

6. **Expense Categories** (Left column)
   - Pie chart
   - Spending breakdown

7. **Billing Status** (Right column)
   - Doughnut chart
   - Paid/Pending/Overdue

#### Apply Filters
1. Click **"Filters"** button in top-right
2. Select date range (default: Last 30 Days)
3. Choose comparison type (default: Previous Period)
4. Pick category filter (default: All Categories)
5. Click **"Reset"** to restore defaults

All charts update instantly when filters change!

#### View Data Tables
Scroll down to see two expandable tables:

1. **Top Performing Metrics**
   - Click "Expand" to see details
   - Shows current vs previous values
   - Includes sparkline trends

2. **Category Breakdown**
   - Click "Expand" for details
   - Shows counts, percentages, and values
   - Visual progress bars

#### Refresh Data
- Click the **refresh icon** (?) in top-right
- All data reloads from database
- Useful if data changes while page is open

## ?? Common Tasks

### Task 1: Check Revenue Growth
1. Go to Analytics page
2. Look at "Total Revenue" card (top-left, blue)
3. Note the trend arrow (? or ?) and percentage
4. Scroll to "Revenue vs Expenses" chart
5. Compare green (revenue) vs red (expenses) bars

### Task 2: Analyze Ticket Performance
1. Look at "Ticket Trends" chart at top
2. Switch to "Bar" view for clearer daily volumes
3. Check "Priority Distribution" chart
4. Note if High/Critical priorities are too many
5. Check "Resolution Rate" metric card

### Task 3: Compare This Month vs Last Month
1. Click "Filters" button
2. Set "Date Range" to "Last 30 Days"
3. Set "Compare With" to "Previous Period"
4. Look at "Status Comparison" chart
5. Current period vs previous side-by-side

### Task 4: Track Client Growth
1. Find "Client Growth" metric card (top-right, purple)
2. Check growth percentage
3. Look at "Client Growth" chart below
4. Note the trend line - is it going up?
5. Check monthly new client counts

### Task 5: Monitor Expenses
1. Find "Expense Categories" pie chart
2. Identify largest expense category
3. Check "Revenue vs Expenses" chart
4. Ensure revenue (green) exceeds expenses (red)
5. Take action if expenses too high

## ?? Pro Tips

### Tip 1: Use Comparison Filters
Change the comparison type to see different insights:
- **Previous Period**: See recent changes
- **Same Period Last Year**: See year-over-year growth

### Tip 2: Focus on Categories
If you only want to see billing data:
1. Open Filters
2. Set "Category Filter" to "Billing Only"
3. Only billing-related metrics show

### Tip 3: Hover for Details
Every chart shows more info on hover:
- Exact values
- Percentages
- Date labels

### Tip 4: Toggle Chart Type
On the Ticket Trends chart:
- **Line**: Best for trends
- **Bar**: Best for day-to-day comparison
- **Area**: Best for cumulative view

### Tip 5: Check Trends Regularly
Bookmark the Analytics page and check:
- **Daily**: Ticket trends
- **Weekly**: Client growth, resolution rate
- **Monthly**: Revenue vs expenses

## ?? Troubleshooting

### Problem: Charts not showing
**Solution**: 
- Check browser console (F12) for errors
- Ensure JavaScript is enabled
- Try refreshing the page (Ctrl+F5)
- Use a modern browser (Chrome, Edge, Firefox, Safari)

### Problem: No data in charts
**Solution**:
- Ensure database has tickets, clients, invoices
- Check that you're logged in as Administrator
- Try changing date range to "Last Year"
- Run database setup script if needed

### Problem: Filters not working
**Solution**:
- Click "Reset" to restore defaults
- Refresh the page
- Check browser console for errors
- Ensure you have data for selected period

### Problem: Page loads slowly
**Solution**:
- Check database connection speed
- Reduce date range (use Last 30 Days)
- Close other browser tabs
- Check network connection

## ?? Mobile Usage

If using on phone/tablet:
1. Charts stack vertically
2. Swipe to scroll through
3. Tap charts for tooltips
4. Filters stack vertically
5. Tables scroll horizontally

## ?? Understanding Metrics

### Total Revenue
- Sum of all **paid** invoices
- In selected date range
- Trend shows % change vs comparison period

### Resolution Rate
- Percentage of tickets **Resolved** or **Closed**
- Out of all tickets in period
- Higher is better (target: >85%)

### Avg Response Time
- Average hours from ticket creation to now
- Lower is better (target: <24h)
- Trend arrow is inverted (down is good)

### Client Growth
- Percentage increase in new clients
- Compared to previous period
- Positive means growing customer base

## ?? Chart Interpretation

### Ticket Trends
- **Rising line**: More tickets being created
- **Flat line**: Consistent volume
- **Falling line**: Fewer tickets (good!)

### Revenue vs Expenses
- **Green higher than Red**: Profitable
- **Red higher than Green**: Losing money
- **Gap widening**: Improving profit margin

### Client Growth
- **Upward trend**: Good acquisition
- **Flat trend**: Stagnant growth
- **Downward trend**: Losing clients (bad!)

### Priority Distribution
- **Large Critical slice**: Need urgent action
- **Large Low slice**: Stable, manageable
- **Balanced**: Good workload mix

## ?? You're All Set!

You now know how to:
- ? Access the Analytics page
- ? Read key metrics
- ? Interact with charts
- ? Apply filters
- ? Interpret data
- ? Troubleshoot issues

### Next Steps
1. Explore all charts
2. Try different filters
3. Check daily for insights
4. Share findings with team
5. Make data-driven decisions

## ?? Need Help?

If you encounter issues:
1. Check the Testing Checklist document
2. Review the README documentation
3. Check browser console for errors
4. Contact system administrator

---

**Happy Analyzing!** ???

*Remember: Data-driven decisions lead to better business outcomes.*
