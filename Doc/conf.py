project = u'Miruken'
copyright = u'2017, Michael Dudley'
author = u'Michael Dudley'

extensions = []
source_suffix = '.rst'
master_doc = 'index'
version = u''
release = u''
exclude_patterns = []
pygments_style = 'sphinx'

html_theme = 'default'

todo_include_todos = False
htmlhelp_basename = 'Mirukendoc'

latex_elements = {
}

latex_documents = [
    (master_doc, 'Miruken.tex', u'Miruken Documentation',
     u'Michael Dudley', 'manual'),
]

man_pages = [
    (master_doc, 'miruken', u'Miruken Documentation',
     [author], 1)
]

texinfo_documents = [
    (master_doc, 'Miruken', u'Miruken Documentation',
     author, 'Miruken', 'One line description of project.',
     'Miscellaneous'),
]