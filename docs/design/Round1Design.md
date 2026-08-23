1. get nouns from ticket

- Order
- Quote
- Work
- Acknowledgement
- Line
- Address
- order id
- merchant
- currency
- sku
- pick fee
- weight handling
- destination surcharge
- rate table
- zone table
- weight handling

### states
- valid
- pre-fulfillment
- created



#### tasks
- get sku weight
-  check if order exists: **handler**
- validate address format: **handler**
- validate line count and quantities: **handler**
- validate merchant id (for currency)
- confirm currency conversion rate exists
- get pick rate for sku
- get handling rate for weight
- get zone surcharge
- generate order id
- calculate total charge per sku
- calculate total charge for order
- create order entity
- save order aggregate

**level one** (no more than 4 or 5 activities)

- validate input
- constuct quote
- create order
- save order


no more than 4 - 5 participants/activities per level

sometimes, oftentimes, judging what the right "level" is requires diving below it.  I.e., we have to dive into details to discover when we've gone too deep.  Too many details brings "just enough" details into relief.  But diving into too many details in this non-technical prose style is much less expensive than writing code.  They can be deleted, rewritten, etc