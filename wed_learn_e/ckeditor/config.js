/**
 * @license Copyright (c) 2003-2013, CKSource - Frederico Knabben. All rights reserved.
 * For licensing, see LICENSE.html or http://ckeditor.com/license
 */

CKEDITOR.editorConfig = function( config ) {
	// Define changes to default configuration here. For example:
	//config.language = 'vi';
    //config.uiColor = '#AADC6E';
  
};
CKEDITOR.editorConfig = function (config) {

    config.toolbar = [
        ['Bold', 'Italic', 'Underline'],
        ['Font', 'FontSize'],
        ['TextColor', 'BGColor'],
        ['NumberedList', 'BulletedList'],
        ['Link', 'Unlink']
    ];

};
CKEDITOR.editorConfig = function (config) {
	// ... (Các cấu hình cũ khác nếu có) ...

	// Thiết lập thanh công cụ đầy đủ (Gần giống ảnh 2 của bạn)
	config.toolbar = [
		{ name: 'document', items: ['Source', '-', 'Save', 'NewPage', 'Preview', 'Print', '-', 'Templates'] },
		{ name: 'clipboard', items: ['Cut', 'Copy', 'Paste', 'PasteText', 'PasteFromWord', '-', 'Undo', 'Redo'] },
		{ name: 'editing', items: ['Find', 'Replace', '-', 'SelectAll', '-', 'Scayt'] },
		'/',
		{ name: 'basicstyles', items: ['Bold', 'Italic', 'Underline', 'Strike', 'Subscript', 'Superscript', '-', 'RemoveFormat'] },
		{ name: 'paragraph', items: ['NumberedList', 'BulletedList', '-', 'Outdent', 'Indent', '-', 'Blockquote', 'CreateDiv', '-', 'JustifyLeft', 'JustifyCenter', 'JustifyRight', 'JustifyBlock', '-', 'BidiLtr', 'BidiRtl', 'Language'] },
		{ name: 'links', items: ['Link', 'Unlink', 'Anchor'] },
		{ name: 'insert', items: ['Image', 'Flash', 'Table', 'HorizontalRule', 'Smiley', 'SpecialChar', 'PageBreak', 'Iframe'] },
		'/',
		{ name: 'styles', items: ['Styles', 'Format', 'Font', 'FontSize'] },
		{ name: 'colors', items: ['TextColor', 'BGColor'] }, // Nhóm màu chữ và màu nền
		{ name: 'tools', items: ['Maximize', 'ShowBlocks'] },
		{ name: 'about', items: ['About'] }
	];
};