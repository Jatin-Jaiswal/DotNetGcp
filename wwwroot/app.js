const api = '/api/products';

async function fetchProducts(){
  try {
    const res = await fetch(api);
    if (!res.ok) {
      throw new Error(`HTTP ${res.status}: ${res.statusText}`);
    }
    const data = await res.json();
    const tbody = document.querySelector('#productsTable tbody');
    tbody.innerHTML = '';
    data.forEach(p => {
      const tr = document.createElement('tr');
      tr.innerHTML = `
        <td>${p.id}</td>
        <td>${p.name}</td>
        <td>${p.description ?? ''}</td>
        <td>${p.price.toFixed(2)}</td>
        <td>
          <button data-id="${p.id}" class="edit">Edit</button>
          <button data-id="${p.id}" class="del">Delete</button>
        </td>
      `;
      tbody.appendChild(tr);
    });
    
    // Add event listeners for delete buttons
    document.querySelectorAll('.del').forEach(b => b.addEventListener('click', async e => {
      const id = e.target.dataset.id;
      if (confirm('Are you sure you want to delete this product?')) {
        try {
          const deleteRes = await fetch(`${api}/${id}`, { method: 'DELETE' });
          if (!deleteRes.ok) {
            throw new Error(`HTTP ${deleteRes.status}: ${deleteRes.statusText}`);
          }
          await fetchProducts();
        } catch (error) {
          alert(`Error deleting product: ${error.message}`);
          console.error('Delete error:', error);
        }
      }
    }));
    
    // Add event listeners for edit buttons
    document.querySelectorAll('.edit').forEach(b => b.addEventListener('click', async e => {
      const id = e.target.dataset.id;
      await editProduct(id);
    }));
  } catch (error) {
    console.error('Error fetching products:', error);
    const tbody = document.querySelector('#productsTable tbody');
    tbody.innerHTML = '<tr><td colspan="5" style="text-align: center; color: red;">Error loading products. Please refresh the page.</td></tr>';
  }
}

// Edit product function
async function editProduct(id) {
  try {
    const res = await fetch(`${api}/${id}`);
    if (!res.ok) {
      throw new Error(`HTTP ${res.status}: ${res.statusText}`);
    }
    const product = await res.json();
    
    // Populate form with existing data
    document.getElementById('name').value = product.name;
    document.getElementById('description').value = product.description || '';
    document.getElementById('price').value = product.price;
    
    // Change form to edit mode
    const form = document.getElementById('createForm');
    const submitBtn = form.querySelector('button[type="submit"]');
    const cancelBtn = document.getElementById('cancelBtn') || document.createElement('button');
    
    if (!document.getElementById('cancelBtn')) {
      cancelBtn.id = 'cancelBtn';
      cancelBtn.type = 'button';
      cancelBtn.textContent = 'Cancel';
      cancelBtn.className = 'cancel-btn';
      form.appendChild(cancelBtn);
    }
    
    submitBtn.textContent = 'Update';
    form.dataset.editId = id;
    
    // Add cancel functionality
    cancelBtn.onclick = () => {
      form.reset();
      form.removeAttribute('data-edit-id');
      submitBtn.textContent = 'Create';
      cancelBtn.style.display = 'none';
    };
    
    cancelBtn.style.display = 'inline-block';
  } catch (error) {
    alert(`Error loading product for editing: ${error.message}`);
    console.error('Edit error:', error);
  }
}

// Form submission handler
document.getElementById('createForm').addEventListener('submit', async (e) => {
  e.preventDefault();
  
  const name = document.getElementById('name').value;
  const description = document.getElementById('description').value;
  const price = parseFloat(document.getElementById('price').value);
  
  // Basic validation
  if (!name.trim()) {
    alert('Name is required');
    return;
  }
  if (isNaN(price) || price < 0) {
    alert('Please enter a valid price');
    return;
  }
  
  const form = document.getElementById('createForm');
  const isEdit = form.dataset.editId;
  
  try {
    let response;
    if (isEdit) {
      // Update existing product
      response = await fetch(`${api}/${isEdit}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name, description, price })
      });
    } else {
      // Create new product
      response = await fetch(api, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ name, description, price })
      });
    }
    
    // Check if the response is successful
    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`HTTP ${response.status}: ${errorText}`);
    }
    
    // Reset form and refresh data
    form.reset();
    form.removeAttribute('data-edit-id');
    form.querySelector('button[type="submit"]').textContent = 'Create';
    const cancelBtn = document.getElementById('cancelBtn');
    if (cancelBtn) {
      cancelBtn.style.display = 'none';
    }
    await fetchProducts();
    
  } catch (error) {
    alert(`Error saving product: ${error.message}`);
    console.error('Error:', error);
  }
});

// initial load
fetchProducts();
