using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Collections;
using System.Configuration.Assemblies;
using System.Configuration;
using System.Xml;

namespace NetLog.Logging {
	public class HandlerConfiguration: ConfigurationElement {
		[ConfigurationProperty("className", IsRequired = true)]
		public string ClassName {
			get { return (string)this[ "className" ]; }
			set { this[ "className" ] = value; }
		}
		[ConfigurationProperty("assembly", IsRequired = false )]
		public string AssemblyName {
			get { return (string)this[ "assembly" ]; }
			set { this[ "assembly" ] = value; }
		}
		[ConfigurationProperty("handler", IsKey = true, IsRequired = true)]
		public string Handler {
			get { return (string)this[ "handler" ]; }
			set { this[ "handler" ] = value; }
		}
		[ConfigurationProperty("level", DefaultValue = "ALL", IsRequired = false)]
		public string Level {
			get { return (string)this[ "level" ]; }
			set { this[ "level" ] = value; }
		}
		[ConfigurationProperty("formatter", DefaultValue = "streamFormatter", IsRequired = false)]
		public string Formatter {
			get { return (string)this[ "formatter" ]; }
			set { this[ "formatter" ] = value; }
		}
		[ConfigurationProperty("filter", IsRequired = false)]
		public string Filter {
			get { return (string)this[ "filter" ]; }
			set { this[ "filter" ] = value; }
		}
		[ConfigurationProperty("properties", IsKey = false, IsRequired = false)]
		public PropertyConfigurationElements Properties {
			get { return (PropertyConfigurationElements)this[ "properties" ]; }
			set { this[ "properties" ] = value; }
		}
	}

	public class LoggerConfiguration: ConfigurationElement {
		[ConfigurationProperty("name", IsKey=true, IsRequired = true)]
		public string Name {
			get { return (string)this[ "name" ]; }
			set { this[ "name" ] = value; }
		}
		[ConfigurationProperty("level", IsRequired = true)]
		public string Level {
			get { return (string)this[ "level" ]; }
			set { this[ "level" ] = value; }
		}
		[ConfigurationProperty("className", IsRequired = false)]
		public string ClassName {
			get { return (string)this[ "className" ]; }
			set { this[ "className" ] = value; }
		}
		[ConfigurationProperty("assembly", IsRequired = false)]
		public string AssemblyName {
			get { return (string)this[ "assembly" ]; }
			set { this[ "assembly" ] = value; }
		}
		[ConfigurationProperty("handler", IsRequired = false)]
		public string Handler {
			get { return (string)this[ "handler" ]; }
			set { this[ "handler" ] = value; }
		}
		[ConfigurationProperty("properties", IsKey = false, IsRequired = false)]
		public PropertyConfigurationElements Properties {
			get { return (PropertyConfigurationElements)this[ "properties" ]; }
			set { this[ "properties" ] = value; }
		}
	}

	public class PropertyConfiguration: ConfigurationElement {
		[ConfigurationProperty("name", IsRequired = true)]
		public string Name {
			get { return (string)this[ "name" ]; }
			set { this[ "name" ] = value; }
		}
		[ConfigurationProperty("value", IsRequired = true)]
		public string Value {
			get { return (string)this[ "value" ]; }
			set { this[ "value" ] = value; }
		}
	}
	public class FormatterConfiguration: ConfigurationElement {
		[ConfigurationProperty("className", IsRequired = true)]
		public string ClassName {
			get { return (string)this[ "className" ]; }
			set { this[ "className" ] = value; }
		}
		[ConfigurationProperty("assembly", IsRequired = false)]
		public string AssemblyName {
			get { return (string)this[ "assembly" ]; }
			set { this[ "assembly" ] = value; }
		}
		[ConfigurationProperty("formatter", IsKey = true, IsRequired = true)]
		public string Formatter {
			get { return (string)this[ "formatter" ]; }
			set { this[ "formatter" ] = value; }
		}
		[ConfigurationProperty("properties", IsKey = false, IsRequired = false)]
		public PropertyConfigurationElements Properties {
			get { return (PropertyConfigurationElements)this[ "properties" ]; }
			set { this[ "properties" ] = value; }
		}
	}
	public class FilterConfiguration: ConfigurationElement {
		[ConfigurationProperty("className", IsRequired = true)]
		public string ClassName {
			get { return (string)this[ "className" ]; }
			set { this[ "className" ] = value; }
		}
		[ConfigurationProperty("assembly", IsRequired = false )]
		public string AssemblyName {
			get { return (string)this[ "assembly" ]; }
			set { this[ "assembly" ] = value; }
		}
		[ConfigurationProperty("filter", IsKey = true, IsRequired = true)]
		public string Filter {
			get { return (string)this[ "filter" ]; }
			set { this[ "filter" ] = value; }
		}
		[ConfigurationProperty("properties", IsKey = false, IsRequired = false)]
		public PropertyConfigurationElements Properties {
			get { return (PropertyConfigurationElements)this[ "properties" ]; }
			set { this[ "properties" ] = value; }
		}
	}


	public class NetLogConfigurationSection: ConfigurationSection {

		[ConfigurationProperty("Handlers",
			IsRequired = false,
			IsDefaultCollection = false)]
		public HandlerConfigurationElements Handlers {
			get {
				return (HandlerConfigurationElements)base[ "Handlers" ];
			}
		}

		[ConfigurationProperty("Formatters",
			IsRequired = false,
			IsDefaultCollection = false)]
		public FormatterConfigurationElements Formatters {
			get {
				return (FormatterConfigurationElements)base[ "Formatters" ];
			}
		}

		[ConfigurationProperty("Filters",
			IsRequired = false,
			IsDefaultCollection = false)]
		public FilterConfigurationElements Filters {
			get {
				return (FilterConfigurationElements)base[ "Filters" ];
			}
		}

		[ConfigurationProperty("Loggers",
			IsRequired = false,
			IsDefaultCollection = false)]
		public LoggerConfigurationElements Loggers {
			get {
				return (LoggerConfigurationElements)base[ "Loggers" ];
			}
		}
	}

	[ConfigurationCollection(typeof(PropertyConfiguration),
		AddItemName="property")]
	public class PropertyConfigurationElements: ConfigurationElementCollection {
		protected override ConfigurationElement CreateNewElement() {
			return new PropertyConfiguration();
		}
		protected override object GetElementKey( ConfigurationElement element ) {
			return ( (PropertyConfiguration)element ).Name;
		}
	}

	[ConfigurationCollection(typeof(LoggerConfiguration),
		AddItemName="logger")]
	public class LoggerConfigurationElements: ConfigurationElementCollection {
		protected override ConfigurationElement CreateNewElement() {
			return new LoggerConfiguration();
		}
		protected override object GetElementKey( ConfigurationElement element ) {
			return ( (LoggerConfiguration)element ).Name;
		}
	}

	[ConfigurationCollection(typeof(FilterConfiguration),
		AddItemName="filter")]
	public class FilterConfigurationElements: ConfigurationElementCollection {
		protected override ConfigurationElement CreateNewElement() {
			return new FilterConfiguration();
		}
		protected override object GetElementKey( ConfigurationElement element ) {
			return ( (FilterConfiguration)element ).Filter;
		}
	}
	[ConfigurationCollection(typeof(HandlerConfiguration))]
	public class HandlerConfigurationElements: ConfigurationElementCollection {
		protected override ConfigurationElement CreateNewElement() {
			return new HandlerConfiguration();
		}
		protected override object GetElementKey( ConfigurationElement element ) {
			return ( (HandlerConfiguration)element ).Handler;
		}
	}


	[ConfigurationCollection(typeof(FormatterConfiguration))]
	public class FormatterConfigurationElements: ConfigurationElementCollection {
		protected override ConfigurationElement CreateNewElement() {
			return new FormatterConfiguration();
		}
		protected override object GetElementKey( ConfigurationElement element ) {
			return ( (FormatterConfiguration)element ).Formatter;
		}
	}

}
